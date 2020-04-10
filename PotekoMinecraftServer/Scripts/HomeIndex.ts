import type * as jquery from "jquery"

function onButtonStartStopClicked(action: string, button) : void {
    const serverName = button.attr("data-server-name");
    button.attr("disabled", true);
    $.post(`/home/${action}Server?name=${serverName}`).done(() => {
        button.removeAttr("disabled");
    });
}

function onButtonMachineStartClicked(button) : void {
    const serverName = button.attr("data-server-name");
    button.attr("disabled", true);
    $.post(`/home/StartMachine?name=${serverName}`).done(() => {
        button.removeAttr("disabled");
    });
}

function bindButtonClickEvents(): void {
    $(".btn-machine-start").on("click", function () {
        onButtonMachineStartClicked($(this));
    });

    $(".btn-server-start").on("click", function () {
        onButtonStartStopClicked("Start", $(this));
    });

    $(".btn-server-stop").on("click", function () {
        onButtonStartStopClicked("Stop", $(this));
    });
}

bindButtonClickEvents();