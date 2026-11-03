"""Captures the README screenshots from a running instance with demo data.

Usage: python tools/take_screenshots.py http://localhost:8080 docs/screenshots
"""
import sys
from pathlib import Path

from playwright.sync_api import sync_playwright

BASE = sys.argv[1].rstrip("/")
OUT = Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)

DEMO_EMAIL = "demo@photogallery.local"
DEMO_PASSWORD = "Demo1234"


def wait_for_images(page):
    page.wait_for_load_state("networkidle")
    page.evaluate(
        """() => Promise.all(Array.from(document.images)
            .filter(img => !img.complete)
            .map(img => new Promise(resolve => { img.onload = img.onerror = resolve; })))"""
    )


def shot(page, name, full_page=False):
    wait_for_images(page)
    page.screenshot(path=str(OUT / f"{name}.png"), full_page=full_page)
    print("saved", name)


with sync_playwright() as p:
    browser = p.chromium.launch()
    context = browser.new_context(viewport={"width": 1280, "height": 860}, device_scale_factor=1, color_scheme="light")
    page = context.new_page()

    # Explore page as an anonymous visitor
    page.goto(f"{BASE}/")
    shot(page, "explore")

    # Sign in as the demo user
    page.goto(f"{BASE}/Identity/Account/Login")
    page.fill("#Input_Email", DEMO_EMAIL)
    page.fill("#Input_Password", DEMO_PASSWORD)
    page.click("#login-submit")
    page.wait_for_url(f"{BASE}/**")

    # My albums
    page.goto(f"{BASE}/Albums")
    shot(page, "albums")

    # Album detail (owner view)
    page.click("a.album-card:has-text('Mountains')")
    page.wait_for_url("**/Albums/Details/**")
    shot(page, "album")

    # Photo detail
    page.click(".photo-tile >> nth=0")
    page.wait_for_url("**/Photos/Details/**")
    shot(page, "photo")

    # Upload form
    page.goto(f"{BASE}/Photos/Upload")
    shot(page, "upload")

    # Dark mode variant of the explore page
    dark = browser.new_context(viewport={"width": 1280, "height": 860}, device_scale_factor=1, color_scheme="dark")
    dark_page = dark.new_page()
    dark_page.goto(f"{BASE}/")
    shot(dark_page, "explore-dark")

    # Mobile view
    mobile = browser.new_context(**p.devices["Pixel 7"])
    mobile_page = mobile.new_page()
    mobile_page.goto(f"{BASE}/")
    shot(mobile_page, "explore-mobile")

    browser.close()
