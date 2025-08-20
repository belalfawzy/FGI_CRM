$(document).ready(function() {
    $('#leadsTable').DataTable({
        responsive: true,
        ordering: true,
        order: [[6, 'desc']],
        columnDefs: [
            { responsivePriority: 1, targets: 0 },
            { responsivePriority: 2, targets: -1 },
            { responsivePriority: 3, targets: 1 },
            { responsivePriority: 4, targets: 6 }
        ],
        language: {
            search: "",
            searchPlaceholder: "Search leads...",
            lengthMenu: "Show _MENU_ entries",
            info: "Showing _START_ to _END_ of _TOTAL_ leads",
            infoEmpty: "Showing 0 to 0 of 0 leads",
            infoFiltered: "(filtered from _MAX_ total leads)"
        }
    });

    $('.dataTables_filter input').addClass('form-control');

    $('.call-link').on('click', function(e) {
        if (!/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
            e.preventDefault();
            alert('Please use a mobile device to make calls');
        }
    });

    $('.note-link').on('click', function(e) {
        e.preventDefault();
        var comment = $(this).data('comment');

        if (comment && comment.trim() !== '') {
            $('#modalComment').text(comment);
        } else {
            $('#modalComment').text('This lead doesn\'t have any comments.');
        }

        $('#notesModal').modal('show');
    });
});
