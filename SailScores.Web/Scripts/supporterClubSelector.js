// Automatically populate ClubInitials field when a club is selected in the supporter form
(function() {
    const clubSelector = document.getElementById('clubSelector');
    const initialsField = document.getElementById('ClubInitials');
    
    if (clubSelector && initialsField) {
        clubSelector.addEventListener('change', function(e) {
            const selectedOption = e.target.options[e.target.selectedIndex];
            
            if (selectedOption.value) {
                // Set the club initials from the selected club
                initialsField.value = selectedOption.getAttribute('data-initials') || '';
            } else {
                // Clear the initials if no club is selected
                initialsField.value = '';
            }
        });
    }
})();
