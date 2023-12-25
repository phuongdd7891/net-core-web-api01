export interface IpcRequest {
  responseChannel?: string;
  params?: Record<string, any>;
}

export interface IpcResponse {
  Code: number,
  Data: any
}