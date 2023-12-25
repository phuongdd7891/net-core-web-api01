import { IpcChannelInterface } from "./IpcChannelInterface";
import { IpcMainEvent } from 'electron';
import { IpcRequest } from "../../shared/IpcRequest";
import { execSync } from "child_process";

export class GeneralInfoChannel implements IpcChannelInterface {
    private _channelName: string;

    constructor(channelName: string = 'system-info') {
        this._channelName = channelName;
    }

    getName(): string {
        return this._channelName;
    }

    handle(event: IpcMainEvent, request: IpcRequest): void {
        if (this._channelName == 'system-info') {
            event.reply(request.responseChannel, { kernel: execSync('uname -a').toString() });
        } else {
            event.reply(request.responseChannel, { ...request.params });
        }
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: any): void {
        throw new Error("Method not implemented.");
    }
}