import type * as sr from "@microsoft/signalr";
import type { } from "jquery";

declare var exports;
const signalR = exports.signalR;

interface ResultResponse {
    result: boolean;
    message: string;
}

enum MinecraftBdsStatus {
    Stopped = 0,
    Running = 1,
    NetworkError = 0xFD,
    LocalError = 0xFE,
    Error = 0xFF,
}

enum MachinePowerState {
    Starting = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Deallocating = 4,
    Deallocated = 5,
    LocalError = 0xFE,
    Error = 0xFF,
}

function machinePowerStateToString(powerState: MachinePowerState) {
    return MachinePowerState[powerState];
}

function bdsStatusToString(status: MinecraftBdsStatus) {
    return MinecraftBdsStatus[status];
}

interface MinecraftMachineStatus {
    powerState: MachinePowerState;
    timestamp: string;
}

interface MinecraftServerStatus {
    minecraftBdsStatus: MinecraftBdsStatus;
    online: number;
    max: number;
    players: string[];
    timestamp: string;
}

interface MinecraftEndpointStatus {
    name: string;
    machineStatus: MinecraftMachineStatus;
    serverStatus: MinecraftServerStatus;
}

function neverReach(): never {
    throw new Error("not expected to get here!");
}

enum DisplayClasses {
    Success = "table-success",
    Warning = "table-warning",
    Failure = "table-danger",
}

function getMinecraftBdsStatusClasses(status: MinecraftBdsStatus): string[] {
    switch (status) {
        case MinecraftBdsStatus.Error:
        case MinecraftBdsStatus.LocalError:
        case MinecraftBdsStatus.NetworkError:
            return [DisplayClasses.Failure];
        case MinecraftBdsStatus.Running:
            return [DisplayClasses.Success];
        case MinecraftBdsStatus.Stopped:
            return [DisplayClasses.Warning];
        default:
            return neverReach();
    }
}

function getPowerStateClasses(powerState: MachinePowerState): string[] {
    switch (powerState) {
        case MachinePowerState.Deallocated:
        case MachinePowerState.Deallocating:
        case MachinePowerState.Starting:
        case MachinePowerState.Stopped:
        case MachinePowerState.Stopping:
            return [DisplayClasses.Warning];
        case MachinePowerState.Running:
            return [DisplayClasses.Success];
        case MachinePowerState.LocalError:
        case MachinePowerState.Error:
            return [DisplayClasses.Failure];
        default:
            return neverReach();
    }
}

function updateServers(serverEntries: MinecraftEndpointStatus[]): void {
    serverEntries.forEach(e => updateServerEntry(e));
}

function updateElementClasses(element: JQuery<HTMLElement>, classes: string[]) {
    const oldClasses: string[] = element.data("class");
    if (oldClasses && oldClasses.length !== 0) {
        element.removeClass(oldClasses);
        element.removeData("class");
    }

    element.data("class", classes);
    element.addClass(classes);
}

function setAsNa(element: JQuery<HTMLElement>, hideFailureBackground?: boolean): void {
    element.html("N/A");

    if (!hideFailureBackground) {
        updateElementClasses(element, [DisplayClasses.Failure]);
    }
}

function setContent(element: JQuery<HTMLElement>, content: string, classes?: string[]) {
    element.html(content);
    if (classes) {
        updateElementClasses(element, classes);
    }
}

function setButtonDisabled(element: JQuery<HTMLElement>, disabled: boolean) {
    if (disabled) {
        element.attr("disabled", "true");
    } else {
        element.removeAttr("disabled");
    }
}

function updateServerEntry(entry: MinecraftEndpointStatus): void {
    const name = entry.name;
    const machineStateDiv = $(`#display-machinestate-${name}`);
    const serverStateDiv = $(`#display-serverstate-${name}`);
    const onlineDiv = $(`#display-online-${name}`);
    const maxDiv = $(`#display-max-${name}`);
    const timestampDiv = $(`#display-timestamp-${name}`);

    setContent(
        machineStateDiv,
        machinePowerStateToString(entry.machineStatus.powerState),
        getPowerStateClasses(entry.machineStatus.powerState));

    if (entry.machineStatus.powerState === MachinePowerState.Running) {
        const bdsStatus = entry.serverStatus.minecraftBdsStatus;
        setContent(serverStateDiv,
            bdsStatusToString(bdsStatus),
            getMinecraftBdsStatusClasses(bdsStatus));

        if (bdsStatus === MinecraftBdsStatus.Running) {
            const online = entry.serverStatus.online;
            setContent(onlineDiv, online.toString(), online > 0 ? ["text-success"] : []);
            setContent(maxDiv, entry.serverStatus.max.toString(), []);
        } else {
            setAsNa(onlineDiv);
            setAsNa(maxDiv);
        }
    } else {
        setAsNa(serverStateDiv);
        setAsNa(onlineDiv);
        setAsNa(maxDiv);
    }

    const date = new Date(entry.machineStatus.timestamp);
    timestampDiv.html(`Updated on ${date}`);
}

function alertResponse(response: ResultResponse, operation?: string): void {
    let content: string;
    const op = operation ? operation : "Operation";

    if (response.result) {
        content = `${op} executed successfully!`;
    } else {
        if (response.message) {
            content = `${op} failed: ${response.message}`;
        } else {
            content = `${op} failed`;
        }
    }

    const alert = `<div class="alert ${response.result ? "alert-success" : "alert-danger"} alert-dismissible fade show" role="alert">${content}<button type="button" class="close" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button></div>`;

    $("#status-table-alert-container").prepend(alert);
}

function onButtonStartStopClicked(action: string, button): void {
    const serverName = button.attr("data-server-name");
    setButtonDisabled(button, true);
    $.post(`/home/${action}Server?name=${serverName}`, (data) => {
        alertResponse(data, `${action} minecraft server on "${serverName}"`);
        setButtonDisabled(button, false);
    });
}

function onButtonMachineStartClicked(button): void {
    const serverName = button.attr("data-server-name");
    setButtonDisabled(button, true);
    $.post(`/home/StartMachine?name=${serverName}`, (data) => {
        alertResponse(data, `Start machine "${serverName}"`);
        setButtonDisabled(button, false);
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

const connection = new signalR.HubConnectionBuilder().withUrl("/mcstatus").build();

connection.on("StatusUpdated", updateServers);
connection.start().catch(err => console.log(err));
//connection.send("RequestUpdate");