export interface AIProvider { id: number; name: string; supportsModelListing: boolean; isActive: boolean; }
export interface ConnectedProvider { id: number; providerId: number; providerName: string; isDefault: boolean; }
export interface ConnectProviderRequest { providerId: number; apiKey: string; }
