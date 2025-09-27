$(document).ready(function() {
    // Remove toastr initialization - we'll use our custom toast system

    // Initialize Select2
    $('.select2').select2({
        placeholder: "Select an option",
        allowClear: true,
        width: '100%'
    });

    // Owner phone search functionality
    $('#ClientPhone').on('input', function() {
        var phone = $(this).val().trim();
        if (phone.length >= 3) {
            searchOwnerByPhone(phone);
        }
    });

    function searchOwnerByPhone(phone) {
        $.get('/Lead/SearchOwner', { term: phone })
            .done(function(data) {
                if (data.found) {
                    $('#ClientName').val(data.name);
                    if (window.toastNotification) {
                        window.toastNotification.success('Owner found: ' + data.name);
                    }
                }
            })
            .fail(function() {
                console.log('Owner search failed');
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
                var data = {
                    q: params.term,
                    limit: 10,
                    projectId: $('#ProjectId').val() || 0
                };
                console.log('Sending AJAX request with data:', data);
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
            error: function(xhr, status, error) {
                console.error('Search error:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText,
                    statusCode: xhr.status
                });
                
                var errorMessage = 'Search failed: ' + error;
                if (xhr.responseText) {
                    try {
                        var errorData = JSON.parse(xhr.responseText);
                        if (errorData.error) {
                            errorMessage = errorData.error;
                        }
                    } catch (e) {
                        console.log('Could not parse error response:', e);
                    }
                }
                
                if (window.toastNotification) {
                    window.toastNotification.error(errorMessage);
                }
            },
            cache: false
        },
        templateResult: function(item) {
            if (item.loading) return item.text;
            
            var $result = $('<div class="unit-search-result">');
            $result.append('<div class="unit-text">' + item.text + '</div>');
            if (item.unit && item.unit.location) {
                $result.append('<small class="text-muted">' + item.unit.location + '</small>');
            }
            return $result;
        },
        templateSelection: function(item) {
            return item.text || item.text;
        },
        language: {
            noResults: function () {
                return "No units found";
            },
            searching: function () {
                return "Searching...";
            },
            inputTooShort: function () {
                return "Please enter at least 1 character";
            }
        }
    });

    // When a unit is selected, load unit details partial
    unitSelect.on('change', function(e) {
        var unitId = $(this).val();
        if (unitId) {
            loadUnitDetails(unitId);
            
            // Auto-fill project if not already selected
            if (!$('#ProjectId').val()) {
                autoFillProjectFromUnit(unitId);
            }
        } else {
            $('#unitDetailsContainer').hide();
        }
    });

    function loadUnitDetails(unitId) {
        console.log('Loading unit details for ID:', unitId);
        
        $.get('/api/units/DetailsPartial/' + unitId)
            .done(function(data) {
                console.log('Unit details loaded successfully');
                $('#unitDetailsContent').html(data);
                $('#unitDetailsContainer').show();
            })
            .fail(function(xhr, status, error) {
                console.error('Failed to load unit details:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
                
                if (window.toastNotification) {
                    window.toastNotification.error('Failed to load unit details: ' + error);
                }
            });
    }

    function autoFillProjectFromUnit(unitId) {
        console.log('Auto-filling project for unit ID:', unitId);
        
        // Get unit details to extract project information
        $.get('/api/units/DetailsPartial/' + unitId)
            .done(function(data) {
                // Extract project ID from the unit details
                var projectId = $(data).find('[data-project-id]').attr('data-project-id');
                if (projectId && projectId !== '0') {
                    console.log('Found project ID:', projectId);
                    $('#ProjectId').val(projectId).trigger('change');
                    
                    if (window.toastNotification) {
                        window.toastNotification.info('Project automatically selected based on unit');
                    }
                }
            })
            .fail(function(xhr, status, error) {
                console.error('Failed to auto-fill project:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
            });
    }

    // Add CSS for unit search results
    if (!$('#unitSearchCSS').length) {
        $('head').append(`
            <style id="unitSearchCSS">
                .unit-search-result {
                    padding: 8px 0;
                }
                .unit-text {
                    font-weight: 500;
                    color: #333;
                }
                .unit-search-result small {
                    display: block;
                    margin-top: 2px;
                }
            </style>
        `);
    }

    // Duplicate phone check
    $('#ClientPhone').on('blur', function() {
        var phone = $(this).val().trim();
        if (phone.length >= 3) {
            $.get('/Lead/SearchClient', { term: phone })
                .done(function(data) {
                    if (data.found) {
                        $('#ClientName').val(data.name);

                        // Show warning message
                        if (window.toastNotification) {
                            window.toastNotification.warning('This phone number already exists in our system');
                        }

                        // Optionally highlight the field
                        $(this).addClass('is-duplicate');
                    }
                });
        }
    });

    // Form validation
    $.validator.setDefaults({
        highlight: function(element) {
            $(element).addClass('is-invalid');
            $(element).closest('.form-group').find('.select2-selection').addClass('is-invalid');
        },
        unhighlight: function(element) {
            $(element).removeClass('is-invalid');
            $(element).closest('.form-group').find('.select2-selection').removeClass('is-invalid');
        },
        errorElement: 'span',
        errorClass: 'invalid-feedback',
        errorPlacement: function(error, element) {
            if (element.hasClass('select2-hidden-accessible')) {
                error.insertAfter(element.next('.select2-container'));
            } else {
                error.insertAfter(element);
            }
        }
    });

    $('#leadForm').validate({
        rules: {
            ClientName: {
                required: true,
                maxlength: 25
            },
            ClientPhone: {
                required: true,
                minlength: 8,
                maxlength: 20
            },
            UnitId: {
                required: true
            }
        },
        messages: {
            ClientName: {
                required: "Please enter client name",
                maxlength: "Client name cannot exceed 25 characters"
            },
            ClientPhone: {
                required: "Please enter phone number",
                minlength: "Phone number must be at least 8 characters",
                maxlength: "Phone number cannot exceed 20 characters"
            },
            UnitId: {
                required: "Please select a unit"
            }
        }
    });

    // Update unit search when project changes
    $('#ProjectId').on('change', function() {
        $('#UnitId').val(null).trigger('change');
        $('#unitDetailsContainer').hide();
        
        // Clear and reinitialize the unit select2 to refresh search scope
        $('#UnitId').empty().append('<option value="">Search by Unit Code, Location, Owner, Price, Area, Type...</option>');
    });


    // Form submission handler
    $('#leadForm').on('submit', function(e) {
        e.preventDefault();

        if (!$(this).valid()) return false;

        var $btn = $('#saveButton');
        $btn.prop('disabled', true);
        $('#saveIcon').hide();
        $('#saveSpinner').show();
        $('#saveText').text('Saving...');

        var formData = new FormData(this);

        $.ajax({
            url: '/Lead/Create',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                console.log('Lead creation response:', response);
                console.log('Response type:', typeof response);
                console.log('Response success:', response ? response.success : 'undefined');
                
                // Reset button state first
                $btn.prop('disabled', false);
                $('#saveIcon').show();
                $('#saveSpinner').hide();
                $('#saveText').text('Save Lead');
                
                // Check if response is valid and has success property
                if (response && typeof response === 'object' && response.success === true) {
                    console.log('Success case - showing success toast');
                    if (window.toastNotification) {
                        window.toastNotification.success(response.message || 'Lead saved successfully!');
                    }
                    // Reset form instead of redirecting
                    $('#leadForm')[0].reset();
                    $('#UnitId').val(null).trigger('change');
                    $('#unitDetailsContainer').hide();
                } else {
                    console.log('Error case - showing error toast');
                    var errorMessage = 'Error creating lead';
                    if (response && response.message) {
                        errorMessage = response.message;
                    }
                    
                    if (window.toastNotification) {
                        window.toastNotification.error(errorMessage);
                        if (response && response.errors) {
                            response.errors.forEach(function(error) {
                                window.toastNotification.error(error);
                            });
                        }
                    }
                }
            },
            error: function(xhr, status, error) {
                console.error('Lead creation error:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
                
                var errorMessage = 'Failed to save lead. Please try again.';
                try {
                    var response = JSON.parse(xhr.responseText);
                    if (response && response.message) {
                        errorMessage = response.message;
                    }
                } catch (e) {
                    // Use default error message
                }
                
                if (window.toastNotification) {
                    window.toastNotification.error(errorMessage);
                }
                $btn.prop('disabled', false);
                $('#saveIcon').show();
                $('#saveSpinner').hide();
                $('#saveText').text('Save Lead');
            }
        });
    });
});
