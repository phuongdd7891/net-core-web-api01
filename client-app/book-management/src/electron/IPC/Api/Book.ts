import { Net } from "electron";
import { IpcRequest } from "../../../shared/IpcRequest";
import { BaseApiChannel, apiEndpointKey, apiMethodKey } from "../BaseApiChannel";
import { NetUtils } from "../../../utils";

export class BookApiChannel extends BaseApiChannel {

    constructor() {
        super("book");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        if (request.params?.[apiEndpointKey]) {
            const reqMethod = request.params?.[apiMethodKey] ?? 'get';
            (reqMethod == 'post' ? NetUtils.postRequest(request.params[apiEndpointKey], request, net) : NetUtils.getRequest(request.params[apiEndpointKey], request, net))
                .then(response => {
                    event.reply(request.responseChannel, response);
                })
        }
    }

}