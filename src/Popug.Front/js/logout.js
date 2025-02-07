// Optional: logout function (if needed in sidebar)
function logout(event) {
    event.preventDefault();
    fetch("https://localhost:7056/logout", {
        method: "GET",
        credentials: "include"
    })
        .then(response => {
        if (response.ok) {
            window.location.href = '/auth/login';
        } else {
            throw new Error("Logout failed!");
        }
        })
        .catch(error => alert("Logout failed!"));
    }