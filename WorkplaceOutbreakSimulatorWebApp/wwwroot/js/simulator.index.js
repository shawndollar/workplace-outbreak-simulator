// simulator/index js
$(document).ready(function () {

    displaySimulatorCompleted();
    
});

function displaySimulatorCompleted() {
    var el = document.getElementById("SimulatorData.IsSimulatorComplete");
    if (el !== null && el.value == "true") {
        toastr.success("The Simulation is Complete. You can now download the employee log files (one after the other).");
    }
}

function disableInput() {

    $("main_run_btn").prop("disabled", true);
    $("#main_run_form :input").not(":button").prop("disabled", true);
};
