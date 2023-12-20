import { IpcChannelInterface } from "./IpcChannelInterface";
import { IpcMainEvent } from 'electron';
import { IpcRequest } from "../../shared/IpcRequest";
import { execSync } from "child_process";

export class SystemInfoChannel implements IpcChannelInterface {
    getName(): string {
        return 'system-info';
    }

    handle(event: IpcMainEvent, request: IpcRequest): void {
        event.reply(request.responseChannel, { kernel: execSync('uname -a').toString() });
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: any): void {
        throw new Error("Method not implemented.");
    }
}