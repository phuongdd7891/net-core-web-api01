export interface IpcRequest {
  responseChannel?: string;
  params?: Record<string, any>;
}