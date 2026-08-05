export interface AIProvider {
  id: number;
  name: string;
  supportsModelListing: boolean;
  isActive: boolean;
}

export interface ConnectedProvider {
  id: number;
  providerId: number;
  providerName: string;
  isDefault: boolean;
  defaultModel?: string | null;
  supportsModelListing?: boolean;
}

export interface ConnectProviderRequest {
  providerId: number;
  apiKey: string;
}

export interface AIModel {
  id: string;
  name: string;
  supportsVision?: boolean;
  supportsTools?: boolean;
  supportsReasoning?: boolean;
  maxTokens?: number | null;
}
