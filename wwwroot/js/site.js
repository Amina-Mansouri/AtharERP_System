// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function togglePwd(btn) {
    var input = btn.previousElementSibling;
    var show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    btn.querySelector('.eye-on').hidden = !show;
    btn.querySelector('.eye-off').hidden = show;
}