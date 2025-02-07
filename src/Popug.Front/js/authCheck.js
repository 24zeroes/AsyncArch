// Perform the authentication check before displaying the content
fetch("https://localhost:7056/check", {
    method: "GET",
    credentials: "include" // Include cookies if needed
})
.then(response => {
    if (!response.ok) {
        throw new Error("Not authenticated");
    }
    // If authenticated, reveal the content
    document.documentElement.style.display = '';
})
.catch(error => {
    console.error("Authentication check failed:", error);
    // Redirect to the login page if not authenticated
    window.location.href = '/auth/login';
});