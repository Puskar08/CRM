function changeContinureButtonState(isSubmitting) {
  const submitBtn = document.getElementById("btnContinue");
  if (isSubmitting) {
    submitBtn.disabled = true;
    submitBtn.innerHTML = "Submitting...";
  } else {
    submitBtn.disabled = false;
    submitBtn.innerHTML = "Continue";
  }
}
