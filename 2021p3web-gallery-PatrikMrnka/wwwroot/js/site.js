// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//function submitForm() {
//    let form = document.getElementById("form_submit");
//    form.submit();
//}

function showChangeNameDiv() {
    var x = document.getElementById("changeNameDiv");

    if (x.style.display === "block") {
        x.style.display = "none";
    }
    else {
        x.style.display = "block";
    }
}

function createNewAlbum() {
    var album = document.getElementById("createNewAlbum");

    if (album.style.display === "block") {
        album.style.display = "none";
    }
    else {
        album.style.display = "block";
    }
}