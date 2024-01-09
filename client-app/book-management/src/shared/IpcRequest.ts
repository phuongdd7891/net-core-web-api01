export interface IpcRequest {
  responseChannel?: string;
  params?: any;
}

export interface IpcResponse {
  code: number,
  data: any
}