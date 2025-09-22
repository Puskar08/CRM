// Toggle sidebar on button click
document.querySelector("#navbar .toggle-btn").addEventListener("click", () => {
  document.querySelector("aside").classList.toggle("sidebar-open");
});

// Close sidebar on cancel button click
document.querySelector(".cancel-btn").addEventListener("click", () => {
  document.querySelector("aside").classList.remove("sidebar-open");
});

//logout js
const profileButton = document.getElementById("profileButton");
const dropdownMenu = document.getElementById("profileDropdown");
const logoutButton = document.getElementById("logoutButton");

profileButton.addEventListener("click", function (e) {
  e.preventDefault();
  dropdownMenu.classList.toggle("show");
});

document.addEventListener("click", function (e) {
  if (!profileButton.contains(e.target) && !dropdownMenu.contains(e.target)) {
    dropdownMenu.classList.remove("show");
  }
});

logoutButton.addEventListener("click", function (e) {
  e.preventDefault();
  fetch("/Account/Logout", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Requested-With": "XMLHttpRequest",
    },
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        window.location.href = data.redirectUrl;
      } else {
        console.error("Logout failed:", data.message);
      }
    })
    .catch((error) => console.error("Error during logout:", error));
});

// Select all sidebar li elements
const sidebarItems = document.querySelectorAll(".sidebar ul li");

// Add click event listener to each li
sidebarItems.forEach((item) => {
  item.addEventListener("click", function (e) {
    // Remove active class from all li elements
    sidebarItems.forEach((li) => li.classList.remove("active"));

    // Add active class to the clicked li
    this.classList.add("active");

    // Allow default link behavior to handle navigation
    // No need for e.preventDefault() or manual navigation
  });
});

function changeContinueButtonState(isSubmitting) {
  const submitBtn = document.getElementById("btnContinue");
  if (isSubmitting) {
    submitBtn.disabled = true;
    submitBtn.innerHTML = "Submitting...";
  } else {
    submitBtn.disabled = false;
    submitBtn.innerHTML = "Continue";
  }
}
