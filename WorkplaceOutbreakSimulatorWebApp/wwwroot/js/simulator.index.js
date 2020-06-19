// simulator/index js
$(document).ready(function () {
    
});

function disableInput() {

    $("main_run_btn").prop("disabled", true);
    $("#main_run_form :input").not(":button").prop("disabled", true);
};
