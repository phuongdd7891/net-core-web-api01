import { Net } from "electron";
import { IpcRequest } from "../../../shared/IpcRequest";
import { BaseApiChannel } from "../BaseApiChannel";
import { NetUtils } from "../../../utils";

export class BookApiChannel extends BaseApiChannel {

    constructor() {
        super("book");
    }

    handleNet(event: Electron.IpcMainEvent, request: IpcRequest, net: Net): void {
        NetUtils.getRequest('api/books', request, net).then(response => {
            event.reply(request.responseChannel, response);
        })
    }

}