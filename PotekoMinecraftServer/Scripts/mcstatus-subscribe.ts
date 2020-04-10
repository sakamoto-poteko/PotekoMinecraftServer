import type * as sr from "@microsoft/signalr";
import type { } from "jquery";

declare var exports;
const signalR = exports.signalR;


enum MinecraftBdsStatus {
    Stopped = 0,
    Running = 1,
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

function getMinecraftBdsStatusClasses(status: MinecraftBdsStatus): string[] {
    switch (status) {
        case MinecraftBdsStatus.Error:
        case MinecraftBdsStatus.LocalError:
            return ["table-danger"];
        case MinecraftBdsStatus.Running:
            return ["table-success"];
        case MinecraftBdsStatus.Stopped:
            return ["table-warning"];
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
            return ["table-warning"];
        case MachinePowerState.Running:
            return ["table-success"];
        case MachinePowerState.LocalError:
        case MachinePowerState.Error:
            return ["table-danger"];
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

function updateServerEntry(entry: MinecraftEndpointStatus): void {
    const name = entry.name;
    const machineStateDiv = $(`#display-machinestate-${name}`);
    const serverStateDiv = $(`#display-serverstate-${name}`);
    const onlineDiv = $(`#display-online-${name}`);
    const maxDiv = $(`#display-max-${name}`);
    const timestampDiv = $(`#display-timestamp-${name}`);

    machineStateDiv.html(machinePowerStateToString(entry.machineStatus.powerState));
    const machineStateClasses = getPowerStateClasses(entry.machineStatus.powerState);
    updateElementClasses(machineStateDiv, machineStateClasses);

    serverStateDiv.html(bdsStatusToString(entry.serverStatus.minecraftBdsStatus));
    const serverStateClasses = getMinecraftBdsStatusClasses(entry.serverStatus.minecraftBdsStatus);
    updateElementClasses(serverStateDiv, serverStateClasses);

    const online = entry.serverStatus.online;
    onlineDiv.html(entry.serverStatus.online.toString());
    if (online > 0) {
        updateElementClasses(onlineDiv, ["text-success"]);
    } else {
        updateElementClasses(onlineDiv, []);
    }

    maxDiv.html(entry.serverStatus.max.toString());

    const date = new Date(entry.machineStatus.timestamp);
    timestampDiv.html(`Updated on ${date}`);
}

const connection = new signalR.HubConnectionBuilder().withUrl("/mcstatus").build();

connection.on("StatusUpdated", updateServers);
connection.start().catch(err => console.log(err));
//connection.send("RequestUpdate");