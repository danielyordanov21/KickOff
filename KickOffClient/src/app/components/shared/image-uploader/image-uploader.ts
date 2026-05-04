import { CommonModule } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';

export interface PendingImageSelection {
  file?: File;
  blobName?: string;
  previewUrl: string;
}

@Component({
  selector: 'image-uploader',
  imports: [CommonModule],
  templateUrl: './image-uploader.html',
  styleUrl: './image-uploader.scss',
})
export class ImageUploader {
  private static readonly MaxImageSizeBytes = 8 * 1024 * 1024;

  changed = output<PendingImageSelection[]>();

  label = input('Project Images');
  helperText = input('Upload one or more images.');
  maxFiles = input(6);
  images = input<PendingImageSelection[]>([]);

  protected readonly error = signal<string | null>(null);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);

    if (files.length === 0) {
      return;
    }

    const remainingSlots = Math.max(0, this.maxFiles() - this.images().length);
    if (remainingSlots <= 0) {
      this.error.set(`You can upload up to ${this.maxFiles()} images.`);
      input.value = '';
      return;
    }

    const selectedFiles = files.slice(0, remainingSlots);
    this.error.set(null);

    const oversizeFile = selectedFiles.find(file => file.size > ImageUploader.MaxImageSizeBytes);
    if (oversizeFile) {
      this.error.set('Each image must be 8MB or smaller.');
      input.value = '';
      return;
    }

    const invalidFile = selectedFiles.find(file => !file.type.startsWith('image/'));
    if (invalidFile) {
      this.error.set('Only image files are allowed.');
      input.value = '';
      return;
    }

    const nextImages = [
      ...this.images(),
      ...selectedFiles.map(file => ({
        file,
        previewUrl: URL.createObjectURL(file)
      }))
    ];

    this.changed.emit(nextImages);
    input.value = '';
  }

  protected removeImage(index: number): void {
    const imageToRemove = this.images()[index];
    if (imageToRemove?.file) {
      URL.revokeObjectURL(imageToRemove.previewUrl);
    }

    const remainingImages = this.images().filter((_, currentIndex) => currentIndex !== index);
    this.changed.emit(remainingImages);
  }
}
