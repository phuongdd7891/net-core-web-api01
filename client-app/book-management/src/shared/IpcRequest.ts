export interface IpcRequest {
  responseChannel?: string;
  params?: Record<string, any>;
}

export interface IpcResponse {
  code: number,
  data: any
}