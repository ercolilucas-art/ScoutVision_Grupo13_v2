document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("togglePassword");
    const input = document.getElementById("passwordInput");

    if (toggle && input) {
        toggle.addEventListener("click", () => {
            input.type = input.type === "password" ? "text" : "password";
        });
    }
});
