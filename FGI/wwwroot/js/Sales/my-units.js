// my-units.js
$(document).ready(function () {
    // Handle View Details button click
    $('.unit-card').on('click', function () {
        var unitId = $(this).data('id');
        loadUnitDetails(unitId);
    });

    // Close details section
    $('#closeDetails').on('click', function () {
        $('#unitDetailsContent').fadeOut(300, function () {
            $('#noUnitSelected').fadeIn(300);
        });
        $('.unit-card').removeClass('active');
    });

    function loadUnitDetails(unitId) {
        // Show loading indicator
        $('#noUnitSelected').hide();
        $('#unitDetailsContent').show().html('<div class="text-center py-5"><i class="fas fa-spinner fa-spin fa-2x"></i><p>Loading data...</p></div>');

        // Add active class to selected card
        $('.unit-card').removeClass('active');
        $(`.unit-card[data-id="${unitId}"]`).addClass('active');

        var url = (currentUserRole === "Sales")
            ? '/Sales/GetUnitDetails'
            : '/Lead/GetUnitDetails';

        $.ajax({
            url: url,
            type: 'GET',
            data: { id: unitId },
            success: function (data) {
                $('#unitDetailsContent').html(data).hide().fadeIn(400);
            },
            error: function () {
                $('#unitDetailsContent').html('<div class="text-center py-4"><i class="fas fa-exclamation-circle fa-2x text-danger"></i><p>Error loading unit details.</p></div>');
            }
        });
    }

    // Hide details section by default
    $('#unitDetailsContent').hide();
});