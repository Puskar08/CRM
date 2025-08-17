// Toggle sidebar on button click
document.querySelector('#navbar .toggle-btn').addEventListener('click', () => {
    document.querySelector('aside').classList.toggle('sidebar-open');
});

// Close sidebar on cancel button click
document.querySelector('.cancel-btn').addEventListener('click', () => {
    document.querySelector('aside').classList.remove('sidebar-open');
});