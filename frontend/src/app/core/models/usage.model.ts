export interface ProviderUsage {
  providerName: string;
  totalCost: number;
  totalTokens: number;
  requestCount: number;
}

export interface ModelUsage {
  model: string;
  totalTokens: number;
  requestCount: number;
}

export interface UsageStats {
  costByProvider: ProviderUsage[];
  tokensByProvider: ProviderUsage[];
  mostUsedWorkflowModels: ModelUsage[];
}
