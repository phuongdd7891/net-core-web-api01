export interface IpcRequest {
  responseChannel?: string;
  params?: any;
}

export interface IpcResponse {
  code: string,
  data: any,
  success: boolean
}