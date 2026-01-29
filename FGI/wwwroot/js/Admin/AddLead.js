// Admin AddLead JavaScript - Same as Marketing CreateLead

$(document).ready(function() {
    // Initialize Select2 for Project dropdown
    $('#ProjectId').select2({
        placeholder: "-- Search All Projects --",
        allowClear: true,
        width: '100%'
    });

    // Client phone input behavior
    $('#ClientPhone').on('input', function() {
        var phone = $(this).val().trim();
        var clientNameHint = $('#clientNameHint');

        if (phone.length >= 3) {
            searchOwnerByPhone(phone);
        } else {
            clientNameHint.text('Enter client name (max 25 chars)');
        }
    });

    function searchOwnerByPhone(phone) {
        $.get('/Admin/SearchOwner', { term: phone })
            .done(function(data) {
                var clientNameField = $('#ClientName');
                var clientNameHint = $('#clientNameHint');
                
                if (data.found) {
                    // Found in database - fill name and make it readonly
                    clientNameField.val(data.name).prop('readonly', true);

                    var message = '';
                    var toastMessage = '';

                    if (data.searchType === 'owner_phone' || data.searchType === 'owner_name') {
                        message = 'Owner found: ' + data.name;
                        toastMessage = 'Owner found: ' + data.name;
                    } else if (data.searchType === 'client_phone' || data.searchType === 'client_name') {
                        message = 'Client found: ' + data.name;
                        toastMessage = 'Client found: ' + data.name;
                    } else {
                        message = 'Found: ' + data.name;
                        toastMessage = 'Found: ' + data.name;
                    }

                    clientNameHint.text(message);
                    if (window.toastNotification) {
                        window.toastNotification.success(toastMessage);
                    } else {
                        toastr.success(toastMessage);
                    }
                } else {
                    // Not found - enable field for manual entry
                    clientNameField.prop('readonly', false).val('');
                    clientNameHint.text('Not found in database. Enter client name manually');
                }
            })
            .fail(function() {
                console.log('Search failed');
                // On error, keep field enabled for manual entry
                $('#ClientName').prop('readonly', false);
                $('#clientNameHint').text('Search failed. Enter client name manually');
            });
    }

    // Unit search functionality with new API
    var unitSelect = $('#UnitId').select2({
        placeholder: "Search by Unit Code, Location, Owner, Price, Area, Type...",
        minimumInputLength: 1,
        width: '100%',
        ajax: {
            url: '/api/units/search',
            dataType: 'json',
            delay: 300,
            data: function(params) {
                var projectId = $('#ProjectId').val();
                var data = {
                    q: params.term,
                    limit: 10,
                    projectId: projectId || null  // Only search in specific project if manually selected
                };
                console.log('Sending AJAX request with data:', data);
                console.log('Project manually selected:', projectId ? 'Yes' : 'No');
                return data;
            },
            processResults: function(data) {
                console.log('Received data:', data);
                console.log('Data type:', typeof data);
                console.log('Data keys:', Object.keys(data || {}));
                
                if (!data) {
                    console.log('No data received');
                    return { results: [] };
                }
                
                if (!data.items) {
                    console.log('No items property in data');
                    console.log('Available properties:', Object.keys(data));
                    return { results: [] };
                }
                
                console.log('Items count:', data.items.length);
                console.log('First item:', data.items[0]);
                
                var results = data.items.map(function(item) {
                    console.log('Processing item:', item);
                    var displayText = item.ownerName + ' — ' + 
                                    (item.price > 0 ? item.price.toLocaleString() + ' ' + item.currency : 'N/A') + ' — ' + 
                                    (item.area > 0 ? item.area + 'm²' : 'N/A') + ' — ' + 
                                    item.type;
                    
                    return {
                        id: item.id,
                        text: displayText,
                        unit: item
                    };
                });
                
                console.log('Processed results:', results);
                return { results: results };
            },
            cache: true
        },
        templateResult: function(unit) {
            if (unit.loading) {
                return unit.text;
            }
            
            var $container = $(
                "<div class='select2-result-unit clearfix'>" +
                "<div class='select2-result-unit__meta'>" +
                "<div class='select2-result-unit__title'></div>" +
                "<div class='select2-result-unit__description'></div>" +
                "</div>" +
                "</div>"
            );
            
            $container.find('.select2-result-unit__title').text(unit.text);
            $container.find('.select2-result-unit__description').text(unit.unit ? 
                (unit.unit.ownerName || 'No Owner') + ' - ' + 
                (unit.unit.price ? unit.unit.price.toLocaleString() + ' ' + unit.unit.currency : 'No Price') + ' - ' + 
                (unit.unit.area || 'No Area') + ' - ' + 
                (unit.unit.type || 'No Type') : '');
            
            return $container;
        },
        templateSelection: function(unit) {
            return unit.text || unit.id;
        }
    });

    // Handle unit selection
    unitSelect.on('select2:select', function (e) {
        var data = e.params.data;
        if (data && data.unit) {
            loadUnitDetails(data.unit.id);
            autoFillProjectFromUnit(data.unit);  // Always auto-fill project from unit
        }
    });

    // Clear unit details when selection is cleared
    unitSelect.on('select2:clear', function (e) {
        $('#unitDetailsContainer').empty();
        $('#ProjectId').val('').trigger('change');  // Clear project when unit is cleared
    });

    function loadUnitDetails(unitId) {
        if (!unitId) return;
        
        $.get('/api/units/DetailsPartial/' + unitId)
            .done(function(data) {
                $('#unitDetailsContainer').html(data);
            })
            .fail(function() {
                console.log('Failed to load unit details');
                $('#unitDetailsContainer').html('<div class="alert alert-warning">Failed to load unit details</div>');
            });
    }

    function autoFillProjectFromUnit(unitData) {
        if (unitData && unitData.projectId) {
            console.log('Auto-filling project ID:', unitData.projectId);
            $('#ProjectId').val(unitData.projectId);
        }
    }

    // Update unit search when project changes
    $('#ProjectId').on('change', function() {
        $('#UnitId').val(null).trigger('change');
        $('#unitDetailsContainer').empty();

        // Clear and reinitialize the unit select2 to refresh search scope
        $('#UnitId').empty().append('<option value="">Search by Unit Code, Location, Owner, Price, Area, Type...</option>');
    });

    // Form submission with toast notifications
    $('#leadForm').on('submit', function(e) {
        e.preventDefault();
        
        var form = this;
        var submitBtn = $(form).find('button[type="submit"]');
        var originalText = submitBtn.html();
        
        // Disable submit button
        submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving...');
        
        // Log form data
        console.log('=== ADMIN FORM SUBMISSION START ===');
        console.log('ClientName:', $('#ClientName').val(), '(Length:', $('#ClientName').val()?.length || 0, ')');
        console.log('ClientPhone:', $('#ClientPhone').val(), '(Length:', $('#ClientPhone').val()?.length || 0, ')');
        console.log('UnitId:', $('#UnitId').val());
        console.log('ProjectId:', $('#ProjectId').val());
        console.log('Comment:', $('#Comment').val(), '(Length:', $('#Comment').val()?.length || 0, ')');
        console.log('AssignedToId:', $('#AssignedToId').val());
        console.log('CurrentStatus:', $('#CurrentStatus').val());
        
        // Check for empty required fields
        if (!$('#ClientName').val() || $('#ClientName').val().trim() === '') {
            console.error('ERROR: ClientName is empty!');
        }
        if (!$('#ClientPhone').val() || $('#ClientPhone').val().trim() === '') {
            console.error('ERROR: ClientPhone is empty!');
        }
        
        $.ajax({
            url: $(form).attr('action'),
            type: 'POST',
            data: $(form).serialize(),
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                console.log('=== ADMIN LEAD CREATE RESPONSE ===');
                console.log('Response:', response);
                console.log('Response type:', typeof response);
                console.log('Response success:', response ? response.success : 'undefined');
                
                if (response.success === true) {
                    // Show success toast
                    if (window.toastNotification) {
                        window.toastNotification.success('Lead created successfully!');
                    } else {
                        toastr.success('Lead created successfully!');
                    }
                    
                    // Reset form
                    form.reset();
                    $('#unitDetailsContainer').empty();
                    $('#UnitId').val(null).trigger('change');
                    $('#ProjectId').val('').trigger('change');
                } else {
                    // Show error toast
                    var errorMsg = response.message || 'Error creating lead';
                    if (window.toastNotification) {
                        window.toastNotification.error(errorMsg);
                    } else {
                        toastr.error(errorMsg);
                    }
                }
            },
            error: function(xhr, status, error) {
                console.error('=== ADMIN AJAX ERROR ===');
                console.error('Status:', status);
                console.error('Error:', error);
                console.error('Response Text:', xhr.responseText);
                console.error('Status Code:', xhr.status);
                console.error('Response Headers:', xhr.getAllResponseHeaders());
                
                var errorMsg = 'Error creating lead';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try {
                        var response = JSON.parse(xhr.responseText);
                        if (response.message) {
                            errorMsg = response.message;
                        }
                    } catch (e) {
                        // Use default error message
                    }
                }
                
                if (window.toastNotification) {
                    window.toastNotification.error(errorMsg);
                } else {
                    toastr.error(errorMsg);
                }
            },
            complete: function() {
                // Re-enable submit button
                submitBtn.prop('disabled', false).html(originalText);
            }
        });
    });
});
