$(document).ready(function() {
    // Initialize price per sqm on page load
    $('#Price').trigger('input');

    // ========== UNIT SEARCH FUNCTIONALITY ========== //
    
    // Unit search functionality
    $('#searchUnitBtn').click(searchUnit);
    $('#unitSearch').keypress(function(e) {
        if (e.which === 13) {
            searchUnit();
            return false;
        }
    });

    // Use selected unit
    $('#useSelectedUnit').click(function() {
        const unitId = $(this).data('unit-id');
        const unitCode = $(this).data('unit-code');

        $('#selectedUnitId').val(unitId);
        $('#selectedUnitText').text(unitCode);
        $('#selectedUnitInfo').show();
        $('#unitSearchResults').hide();
        $('#unitSearch').val('');
    });

    // Clear unit selection
    $('#clearUnitSelection').click(function() {
        $('#selectedUnitId').val('');
        $('#selectedUnitInfo').hide();
    });

    function searchUnit() {
        const searchTerm = $('#unitSearch').val().trim();
        if (!searchTerm) {
            if (window.toastNotification) {
                window.toastNotification.warning('Please enter search term');
            } else {
                alert('Please enter search term');
            }
            return;
        }

        // AJAX call to search for unit
        $.ajax({
            url: '/Lead/MyUnits',
            type: 'GET',
            data: { 
                term: searchTerm,
                projectId: $('#ProjectId').val() || 0
            },
            success: function(response) {
                if (response && response.length > 0) {
                    // Display found units
                    let unitsHtml = '';
                    response.forEach(function(unit) {
                        unitsHtml += `
                            <div class="unit-search-item border p-2 mb-2 rounded">
                                <div class="d-flex justify-content-between align-items-center">
                                    <div>
                                        <strong>${unit.unitCode || 'N/A'}</strong> - 
                                        ${unit.ownerName || 'No Owner'} - 
                                        <span class="text-success">₪${unit.price ? unit.price.toLocaleString() : 'N/A'}</span> - 
                                        ${unit.area || 'N/A'}m² - 
                                        ${unit.type || 'N/A'}
                                    </div>
                                    <button type="button" class="btn btn-sm btn-primary use-unit-btn" 
                                            data-unit-id="${unit.id}" 
                                            data-unit-code="${unit.unitCode || 'N/A'}">
                                        Use
                                    </button>
                                </div>
                                <div class="small text-muted">
                                    Location: ${unit.location || 'N/A'} | 
                                    Bedrooms: ${unit.bedrooms || 'N/A'} | 
                                    Bathrooms: ${unit.bathrooms || 'N/A'}
                                </div>
                            </div>
                        `;
                    });
                    
                    $('#foundUnitsList').html(unitsHtml);
                    $('#unitSearchResults').show();
                    $('#unitNotFound').hide();
                    $('#addNewUnitForm').hide();

                    // Add click handlers for use buttons
                    $('.use-unit-btn').click(function() {
                        const unitId = $(this).data('unit-id');
                        const unitCode = $(this).data('unit-code');
                        
                        $('#selectedUnitId').val(unitId);
                        $('#selectedUnitText').text(unitCode);
                        $('#selectedUnitInfo').show();
                        $('#unitSearchResults').hide();
                        $('#unitSearch').val('');
                    });
                } else {
                    // Show "not found" message
                    $('#unitSearchResults').hide();
                    $('#unitNotFound').show();
                    $('#addNewUnitForm').hide();
                }
            },
            error: function(xhr, status, error) {
                if (window.toastNotification) {
                    window.toastNotification.error('Error searching for units: ' + error);
                } else {
                    alert('Error searching for units: ' + error);
                }
            }
        });
    }

    // Validate UnitCode only when Project is selected
    $('#ProjectId').change(function() {
        if ($(this).val()) {
            $('#UnitCode').rules('add', {
                required: false
            });
        } else {
            $('#UnitCode').rules('add', {
                required: false
            });
        }
    });

    // Initial validation setup
    $('#unitForm').validate({
        rules: {
            UnitCode: {
                required: function() {
                    return $('#ProjectId').val() !== '';
                }
            }
        },
        messages: {
            UnitCode: {
                required: "Unit code is required when project is selected"
            }
        }
    });

    // ========== OWNER SELECTION FUNCTIONALITY ========== //

    // Owner search functionality
    $('#searchOwnerBtn').click(searchOwner);
    $('#ownerSearch').keypress(function(e) {
        if (e.which === 13) {
            searchOwner();
            return false;
        }
    });

    // Use selected owner
    $('#useSelectedOwner').click(function() {
        const ownerId = $(this).data('owner-id');
        const ownerName = $(this).data('owner-name');

        $('#selectedOwnerId').val(ownerId);
        $('#selectedOwnerText').text(ownerName);
        $('#selectedOwnerInfo').show();
        $('#ownerSearchResults').hide();
        $('#ownerSearch').val('');
    });

    // Clear owner selection
    $('#clearOwnerSelection').click(function() {
        $('#selectedOwnerId').val('');
        $('#selectedOwnerInfo').hide();
    });

    // Show add owner form
    $('#showAddOwnerForm').click(function() {
        $('#ownerNotFound').hide();
        $('#addNewOwnerForm').show();
    });

    // Save new owner
    $('#saveNewOwner').click(function() {
        const name = $('#newOwnerName').val().trim();
        const phone = $('#newOwnerPhone').val().trim();
        const email = $('#newOwnerEmail').val().trim();

        if (!name) {
            if (window.toastNotification) {
                window.toastNotification.warning('Owner name is required');
            } else {
                alert('Owner name is required');
            }
            return;
        }

        // AJAX call to save new owner
        $.ajax({
            url: '/Lead/AddOwnerAjax',
            type: 'POST',
            data: {
                name: name,
                phone: phone,
                email: email,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    // Set the new owner as selected
                    $('#selectedOwnerId').val(response.ownerId);
                    $('#selectedOwnerText').text(name);
                    $('#selectedOwnerInfo').show();
                    $('#addNewOwnerForm').hide();
                    $('#newOwnerName, #newOwnerPhone, #newOwnerEmail').val('');

                    // Refresh the owners dropdown if needed
                    if (response.ownerId && response.name) {
                        var $ownerDropdown = $('#OwnerId');
                        if ($ownerDropdown.length) {
                            $ownerDropdown.append(new Option(response.name, response.ownerId));
                            $ownerDropdown.val(response.ownerId);
                        }
                    }

                    if (window.toastNotification) {
                        window.toastNotification.success('Owner added successfully');
                    }
                } else {
                    if (window.toastNotification) {
                        window.toastNotification.error(response.message || 'Error saving owner');
                    } else {
                        alert(response.message || 'Error saving owner');
                    }
                }
            },
            error: function(xhr, status, error) {
                if (window.toastNotification) {
                    window.toastNotification.error('Error saving owner: ' + error);
                } else {
                    alert('Error saving owner: ' + error);
                }
            }
        });
    });

    function searchOwner() {
        const searchTerm = $('#ownerSearch').val().trim();
        if (!searchTerm) {
            if (window.toastNotification) {
                window.toastNotification.warning('Please enter search term');
            } else {
                alert('Please enter search term');
            }
            return;
        }

        // AJAX call to search for owner
        $.ajax({
            url: '/Lead/SearchOwner',
            type: 'GET',
            data: { term: searchTerm },
            success: function(response) {
                if (response.found) {
                    // Display found owner
                    $('#foundOwnerDetails').html(`
                        <p><strong>Name:</strong> ${response.name}</p>
                        <p><strong>Phone:</strong> ${response.phone || 'N/A'}</p>
                        <p><strong>Email:</strong> ${response.email || 'N/A'}</p>
                    `);
                    $('#useSelectedOwner')
                        .data('owner-id', response.id)
                        .data('owner-name', response.name);
                    $('#ownerSearchResults').show();
                    $('#ownerNotFound').hide();
                    $('#addNewOwnerForm').hide();
                } else {
                    // Show "not found" message
                    $('#ownerSearchResults').hide();
                    $('#ownerNotFound').show();
                    $('#addNewOwnerForm').hide();
                }
            },
            error: function(xhr, status, error) {
                if (window.toastNotification) {
                    window.toastNotification.error('Error searching for owner: ' + error);
                } else {
                    alert('Error searching for owner: ' + error);
                }
            }
        });
    }
});
