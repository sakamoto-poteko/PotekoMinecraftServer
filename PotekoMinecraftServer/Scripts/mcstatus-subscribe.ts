declare var signalR: any;

enum MinecraftBdsStatus {
    Stopped = 0,
    Running = 1,
    Error = 0xFF,
}

enum MachinePowerState {
    Starting = 0,
    Running = 1,
    Stopping = 2,
    Stopped = 3,
    Deallocating = 4,
    Deallocated = 5,
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
        case MachinePowerState.Error:
            return ["table-danger"];
        default:
            return neverReach();
    }
}

function updateServers(serverEntries: MinecraftEndpointStatus[]): void {
    serverEntries.forEach(e => updateServerEntry(e));
}

function updateElementClasses(element: HTMLElement, classes: string[]) {
    if (element.dataset.class.length !== 0) {
        const oldClasses = element.dataset.class.split(",");
        element.classList.remove(...oldClasses);
    }

    if (classes.length === 0) {
        element.dataset.class = "";
    } else {
        element.dataset.class = classes.join(",");
        element.classList.add(...classes);
    }
}

function updateServerEntry(entry: MinecraftEndpointStatus): void {
    const name = entry.name;
    const machineStateDiv: HTMLDivElement = document.querySelector(`#display-machinestate-${name}`);
    const serverStateDiv: HTMLDivElement = document.querySelector(`#display-serverstate-${name}`);
    const onlineDiv: HTMLDivElement = document.querySelector(`#display-online-${name}`);
    const maxDiv: HTMLDivElement = document.querySelector(`#display-max-${name}`);
    const timestampDiv: HTMLDivElement = document.querySelector(`#display-timestamp-${name}`);

    machineStateDiv.innerHTML = machinePowerStateToString(entry.machineStatus.powerState);
    const machineStateClasses = getPowerStateClasses(entry.machineStatus.powerState);
    updateElementClasses(machineStateDiv, machineStateClasses);

    serverStateDiv.innerHTML = bdsStatusToString(entry.serverStatus.minecraftBdsStatus);
    const serverStateClasses = getMinecraftBdsStatusClasses(entry.serverStatus.minecraftBdsStatus);
    updateElementClasses(serverStateDiv, serverStateClasses);

    const online = entry.serverStatus.online;
    onlineDiv.innerHTML = entry.serverStatus.online.toString();
    if (online > 0) {
        updateElementClasses(onlineDiv, ["text-success"]);
    } else {
        updateElementClasses(onlineDiv, []);
    }

    maxDiv.innerHTML = entry.serverStatus.max.toString();

    const date = new Date(entry.machineStatus.timestamp);
    timestampDiv.innerHTML = `Updated on ${date}`;
}

const connection = new signalR.HubConnectionBuilder().withUrl("/mcstatus").build();

connection.on("StatusUpdated", updateServers);
connection.start().catch(err => console.log(err));
//connection.send("RequestUpdate");