$(document).ready(function() {

    // Initialize price per sqm on page load
    $('#Price').trigger('input');

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
            alert('Owner name is required');
            return;
        }

        // AJAX call to save new owner
        $.ajax({
            url: '/Sales/AddOwnerAjax',
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
                } else {
                    alert(response.message || 'Error saving owner');
                }
            },
            error: function(xhr, status, error) {
                alert('Error saving owner: ' + error);
            }
        });
    });

    function searchOwner() {
        const searchTerm = $('#ownerSearch').val().trim();
        if (!searchTerm) {
            alert('Please enter search term');
            return;
        }

        // AJAX call to search for owner
        $.ajax({
            url: '/Sales/SearchOwner',
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
                alert('Error searching for owner: ' + error);
            }
        });
    }
});