import { Injectable } from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {environment} from '../../../environments/environment';
import {Observable} from 'rxjs';
import {VideoJson} from '../../models/VideoJson';
import {ConversionResult, ConversionTarget, VideoConversion} from '../../models/VideoConversion';

@Injectable({
  providedIn: 'root'
})
export class YtdlService {

  private baseUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
  ) { }

  public getVideoJson(url: string): Observable<VideoJson> {
    return this.http.get<VideoJson>(this.baseUrl + 'api/download/json', {
      params: new HttpParams().set('ytUrl', encodeURI(url))
    });
  }

  public convertVideo(
    url: string,
    conversionTarget: ConversionTarget,
    title: string,
    artists?: string,
    quality?: number): Observable<ConversionResult> {

    const bod: VideoConversion = {
      url,
      conversionTarget,
      metaDataInfo: {
        title,
        artists,
        quality
      }
    };
    return this.http.post<ConversionResult>(this.baseUrl + 'api/download', bod);
  }
}
