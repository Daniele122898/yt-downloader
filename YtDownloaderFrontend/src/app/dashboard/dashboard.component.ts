import {Component, OnInit} from '@angular/core';
import {faSearch} from '@fortawesome/free-solid-svg-icons';
import {YtdlService} from './services/ytdl.service';
import {VideoJson} from '../models/VideoJson';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {ConversionResult, ConversionTarget} from '../models/VideoConversion';
import {environment} from '../../environments/environment';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  public faSearch = faSearch;
  public youtubeUrl: string;
  public videoJson: VideoJson;
  public convertForm: FormGroup;
  public fetching = false;
  public converting = false;
  public conversionResult: ConversionResult;
  public downloadUrl: string;
  public conversionTarget = ConversionTarget.Mp3;

  public readonly CONVERSION_TARGET_MP3 = ConversionTarget.Mp3;
  public readonly CONVERSION_TARGET_MP4 = ConversionTarget.Mp4;

  public readonly QUALITIES = [1080, 720, 480];

  private baseUrl = environment.apiUrl;

  constructor(
    private ytdlService: YtdlService,
    private fb: FormBuilder,
  ) { }

  ngOnInit(): void {
    this.createForm();
  }

  public getOptionsConversionString(): string {
    switch (this.conversionTarget) {
      case ConversionTarget.Mp3:
        return 'Convert to MP4';
      case ConversionTarget.Mp4:
        return 'Convert to MP3';
    }
  }

  public onYoutubeSearch(): void {
    if (this.fetching || !this.youtubeUrl) {
      return;
    }

    this.fetching = true;
    this.clearFormAndData();

    this.ytdlService.getVideoJson(this.youtubeUrl)
      .subscribe((videoJson) => {
        console.log(videoJson);
        this.videoJson = videoJson;
        this.populateForm();
        this.fetching = false;
      }, err => {
        console.log(err);
        this.fetching = false;
      });
  }

  public toggleConversion(): void {
    if (this.conversionTarget === ConversionTarget.Mp3) {
      this.conversionTarget = ConversionTarget.Mp4;
    } else {
      this.conversionTarget = ConversionTarget.Mp3;
    }
  }

  public convertVideo(): void {
    if (!this.convertForm.valid) {
      return;
    }

    this.conversionResult = undefined;
    this.converting = true;

    this.ytdlService.convertVideo(
      this.youtubeUrl,
      ConversionTarget.Mp3,
      this.convertForm.get('title').value,
      this.convertForm.get('artist').value)
      .subscribe((res) => {
        this.conversionResult = res;
        this.converting = false;
        this.downloadUrl = `${this.baseUrl}file/${this.conversionResult.fileName}`;
      }, err => {
        console.log(err);
        this.converting = false;
      });
  }

  private clearFormAndData(): void {
    this.convertForm.reset();
    this.videoJson = undefined;
    this.conversionResult = undefined;
  }

  private populateForm(): void {
    this.convertForm.get('title').setValue(this.videoJson.title);
    this.convertForm.get('artist').setValue(this.videoJson.artist ?? this.videoJson.creator);
  }

  private createForm(): void {
    this.convertForm = this.fb.group({
      title: ['', Validators.required],
      artist: [''],
      quality: [this.QUALITIES[1]],
    });
  }
}
