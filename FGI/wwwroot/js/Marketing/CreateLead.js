$(document).ready(function() {
    // Initialize toastr
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: "toast-top-right",
        timeOut: 5000
    };

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
                    toastr.success('Owner found: ' + data.name);
                }
            })
            .fail(function() {
                console.log('Owner search failed');
            });
    }

    // Unit search functionality
    var unitSelect = $('#UnitId').select2({
        placeholder: "Search by Unit Code, Location, or Description",
        minimumInputLength: 2,
        width: '100%',
        ajax: {
            url: '/Lead/GetUnits',
            dataType: 'json',
            delay: 300,
            data: function(params) {
                return {
                    projectId: $('#ProjectId').val() || 0,
                    term: params.term,
                    _: new Date().getTime()
                };
            },
            processResults: function(data) {
                return {
                    results: $.map(data, function(item) {
                        return {
                            id: item.id,
                            text: item.text,
                            disabled: item.disabled,
                            unit: item.unit,
                            projectId: item.projectId
                        };
                    })
                };
            },
            error: function(xhr) {
                console.error('Search error:', xhr.responseText);
            },
            cache: true
        },
        templateResult: function(item) {
            if (item.loading) return item.text;
            var $result = $('<span>' + item.text + '</span>');
            if (item.disabled) {
                $result.append(' <span class="badge badge-danger unit-detail-badge">Not Available</span>');
                $result.addClass('text-muted');
            } else {
                $result.append(' <span class="badge badge-success unit-detail-badge">Available</span>');
            }
            return $result;
        },
        templateSelection: function(item) {
            if (item.id === '') return item.text;
            return item.text + (item.disabled ? ' (Not Available)' : '');
        }
    });

    // When a unit is selected, automatically set the project
    unitSelect.on('change', function(e) {
        var unitId = $(this).val();
        if (unitId) {
            // Show loading indicator
            $('#unitDetailsContent').html('<div class="col-12 text-center"><i class="fas fa-spinner fa-spin"></i> Loading...</div>');
            $('#unitDetailsContainer').show();

            // Get unit details
            $.get('/Lead/GetUnitDetails', { id: unitId })
                .done(function(data) {
                    $('#unitDetailsContent').html(data);

                    // If the unit has a project, set it automatically
                    var projectId = $(data).find('[data-project-id]').data('project-id');
                    if (projectId) {
                        $('#ProjectId').val(projectId).trigger('change');
                    }
                })
                .fail(function() {
                    $('#unitDetailsContent').html('<div class="col-12 text-danger">Error loading details</div>');
                });
        } else {
            $('#unitDetailsContainer').hide();
        }
    });

    // Duplicate phone check
    $('#ClientPhone').on('blur', function() {
        var phone = $(this).val().trim();
        if (phone.length >= 3) {
            $.get('/Lead/SearchClient', { term: phone })
                .done(function(data) {
                    if (data.found) {
                        $('#ClientName').val(data.name);

                        // Show warning message
                        toastr.warning('This phone number already exists in our system', 'Duplicate Found', {
                            timeOut: 10000,
                            extendedTimeOut: 5000,
                            closeButton: true
                        });

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
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    toastr.success(response.message);
                    setTimeout(function() {
                        window.location.href = response.redirect || '/Lead/Index';
                    }, 1500);
                } else {
                    toastr.error(response.message);
                    if (response.errors) {
                        response.errors.forEach(function(error) {
                            toastr.error(error);
                        });
                    }
                    $btn.prop('disabled', false);
                    $('#saveIcon').show();
                    $('#saveSpinner').hide();
                    $('#saveText').text('Save Lead');
                }
            },
            error: function() {
                toastr.error('Failed to save lead. Please try again.');
                $btn.prop('disabled', false);
                $('#saveIcon').show();
                $('#saveSpinner').hide();
                $('#saveText').text('Save Lead');
            }
        });
    });
});
