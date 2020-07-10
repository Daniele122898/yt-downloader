export interface VideoConversion {
  url: string;
  conversionTarget: ConversionTarget;
  metaDataInfo: MetaDataInfo;
}

export enum ConversionTarget {
  Mp3,
  Mp4
}

export interface MetaDataInfo {
  title: string;
  artists?: string;
  quality?: number;
}

export interface ConversionResult {
  fileName: string;
  videoTitle?: string;
}
