import {IpcMainEvent, Net} from 'electron';
import {IpcRequest} from "../../shared/IpcRequest";

export interface IpcChannelInterface {
  getName(): string;

  handle(event: IpcMainEvent, request: IpcRequest): void;

  handleNet(event: IpcMainEvent, request: IpcRequest, net: Net): void;
}