$(document).ready(function () {
    let $rows = $("#leadsTable tbody tr");
    let showEntries = 10; // default value

    function applyFilters() {
        let search = $("#searchInput").val().toLowerCase();
        let project = $("#projectFilter").val();
        let salesRep = $("#salesRepFilter").val();
        let status = $("#statusFilter").val();
        let date = $("#dateFilter").val();
        let activeTab = $(".tabs .tab.active").data("status");

        let visibleCount = 0;

        $rows.each(function () {
            let $row = $(this);
            let match = true;

            if (search && !$row.data("search").includes(search)) match = false;
            if (project && $row.data("project") != project) match = false;
            if (salesRep && $row.data("salesrep") != salesRep) match = false;
            if (status && $row.data("status") != status) match = false;
            if (date && $row.data("date") != date) match = false;
            if (activeTab && $row.data("status") != activeTab && activeTab != "") match = false;

            if (match && (showEntries === -1 || visibleCount < showEntries)) {
                $row.show();
                visibleCount++;
            } else {
                $row.hide();
            }
        });
    }

    // Filters change
    $(".filter-control").on("input change", applyFilters);

    // Tabs click
    $(".tabs .tab").click(function () {
        $(".tabs .tab").removeClass("active");
        $(this).addClass("active");
        applyFilters();
    });

    // Show entries dropdown
    $(".entries-option").click(function (e) {
        e.preventDefault();
        showEntries = parseInt($(this).data("value"));
        $("#currentEntries").text(showEntries === -1 ? "All" : showEntries);
        applyFilters();
    });

    // Reset filters
    $("#resetFilters").click(function () {
        $("#searchInput").val("");
        $("#projectFilter").val("").trigger("change");
        $("#salesRepFilter").val("").trigger("change");
        $("#statusFilter").val("").trigger("change");
        $("#dateFilter").val("");
        $(".tabs .tab").removeClass("active");
        $(".tabs .tab[data-status='']").addClass("active");
        showEntries = 10;
        $("#currentEntries").text("10");
        applyFilters();
    });

    // Initial filter
    applyFilters();
});