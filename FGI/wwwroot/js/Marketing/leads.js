$(document).ready(function () {
    // Initialize tab functionality
    $('#leadsTab a').on('click', function (e) {
        e.preventDefault();
        $(this).tab('show');
        applyFilters();
    });

    // Apply filters on search button click or filter change
    $('#searchButton').on('click', applyFilters);
    $('#statusFilter, #assignedFilter').on('change', applyFilters);
    $('#searchInput').on('keyup', function (e) {
        if (e.key === 'Enter') {
            applyFilters();
        }
    });

    // Reset filters
    $('#resetFilters').on('click', function () {
        $('#statusFilter').val('all');
        $('#assignedFilter').val('all');
        $('#searchInput').val('');
        applyFilters();
    });

    function applyFilters() {
        var searchTerm = $('#searchInput').val().toLowerCase();
        var statusFilter = $('#statusFilter').val();
        var assignedFilter = $('#assignedFilter').val();
        var activeTab = $('#leadsTabContent .tab-pane.active');
        var table = activeTab.find('table');

        if (table.length === 0) return;

        table.find('tbody tr').each(function () {
            var $row = $(this);
            var rowText = $row.text().toLowerCase();
            var rowStatus = $row.find('td:eq(5)').text().trim().toLowerCase();
            var rowAssigned = $row.find('td:eq(6)').text().trim().toLowerCase();
            var rowClient = $row.find('td:eq(0)').text().trim().toLowerCase();
            var rowPhone = $row.find('td:eq(1)').text().trim().toLowerCase();
            var rowProject = $row.find('td:eq(2)').text().trim().toLowerCase();

            var matchesSearch = searchTerm === '' ||
                rowText.indexOf(searchTerm) > -1 ||
                rowClient.indexOf(searchTerm) > -1 ||
                rowPhone.indexOf(searchTerm) > -1 ||
                rowProject.indexOf(searchTerm) > -1;

            var matchesStatus = statusFilter === 'all' ||
                rowStatus === statusFilter.toLowerCase();

            var matchesAssigned = assignedFilter === 'all' ||
                rowAssigned.indexOf(assignedFilter.toLowerCase()) > -1;

            if (matchesSearch && matchesStatus && matchesAssigned) {
                $row.show();
            } else {
                $row.hide();
            }
        });

        var visibleRows = table.find('tbody tr:visible').length;
        if (visibleRows === 0) {
            if (!table.next('.no-results-message').length) {
                table.after('<div class="no-results-message p-4 text-center text-muted"><i class="fas fa-search fa-2x mb-3"></i><h5>No matching leads found</h5><p>Try adjusting your filters</p></div>');
            }
        } else {
            table.next('.no-results-message').remove();
        }
    }

    // Apply filters on page load
    applyFilters();
});
