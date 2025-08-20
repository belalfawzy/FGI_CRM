$(document).ready(function () {
    // Handle View Details button click
    $('.view-details').on('click', function () {
        var unitId = $(this).data('id');
        $('#unitDetailsContent').html('<div class="text-center py-5"><i class="fas fa-spinner fa-spin fa-3x"></i><p>Loading data...</p></div>');
        $('#unitDetailsModal').modal('show');

        $.ajax({
            url: '/Sales/GetUnitDetails',
            type: 'GET',
            data: { id: unitId },
            success: function (data) {
                $('#unitDetailsContent').html(data);
            },
            error: function () {
                $('#unitDetailsContent').html('<div class="text-center py-4"><i class="fas fa-exclamation-circle fa-2x text-danger"></i><p>Error loading unit details.</p></div>');
            }
        });
    });

    // Clear modal content when closed
    $('#unitDetailsModal').on('hidden.bs.modal', function () {
        $('#unitDetailsContent').empty();
    });
});