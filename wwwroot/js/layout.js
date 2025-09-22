document.addEventListener("DOMContentLoaded", function () {
  const sidebar = document.getElementById("sidebar");
  const navToggle = document.getElementById("navToggle");

  // Toggle sidebar on button click
  navToggle.addEventListener("click", function () {
    sidebar.classList.toggle("active");

    // Change icon based on sidebar state
    const icon = navToggle.querySelector("i");
    if (sidebar.classList.contains("active")) {
      icon.classList.remove("fa-bars");
      icon.classList.add("fa-times");
    } else {
      icon.classList.remove("fa-times");
      icon.classList.add("fa-bars");
    }
  });

  // Close sidebar when clicking outside on mobile
  document.addEventListener("click", function (event) {
    if (
      window.innerWidth <= 992 &&
      sidebar.classList.contains("active") &&
      !sidebar.contains(event.target) &&
      event.target !== navToggle
    ) {
      sidebar.classList.remove("active");
      const icon = navToggle.querySelector("i");
      icon.classList.remove("fa-times");
      icon.classList.add("fa-bars");
    }
  });

  // Add active class to clicked nav items
  const navItems = document.querySelectorAll(".nav-menu li");
  navItems.forEach((item) => {
    item.addEventListener("click", function () {
      navItems.forEach((i) => i.classList.remove("active"));
      this.classList.add("active");
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
});
