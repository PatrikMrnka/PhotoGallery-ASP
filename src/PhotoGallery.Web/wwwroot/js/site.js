// Ask for confirmation before submitting forms marked with data-confirm="message".
document.addEventListener('submit', (event) => {
    const message = event.target.dataset?.confirm;
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

// Submit the surrounding form when a select marked with data-autosubmit changes (sorting).
document.addEventListener('change', (event) => {
    if (event.target.matches('select[data-autosubmit]')) {
        event.target.form?.submit();
    }
});
