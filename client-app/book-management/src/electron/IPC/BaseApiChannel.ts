import { IpcRequest } from "src/shared/IpcRequest";
import { IpcChannelInterface } from "./IpcChannelInterface";

export const apiHost: string = 'http://localhost:5253';
export const apiNamePrefix: string = 'api-';
export const appSessionKey: string = '__app_session';
export const apiEndpointKey: string = 'endpoint';
export const apiMethodKey: string = 'method';

export abstract class BaseApiChannel implements IpcChannelInterface {
    private _apiName: string;

    constructor(apiName: string) {
        this._apiName = apiName;
    }

    getName(): string {
        return `${apiNamePrefix}${this._apiName}`;
    }

    handle(event: Electron.IpcMainEvent, request: IpcRequest): void {
        throw new Error("Method not implemented.");
    }

    abstract handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Electron.Net): void;
}