import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CarouselModule } from 'primeng/carousel';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { ProjectCard } from '../../sheets/home/project-card/project-card';

@Component({
  selector: 'carousel',
  templateUrl: './carousel.html',
  styleUrl: './carousel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ButtonModule,
    CarouselModule,
    ProjectCard
  ],
})
export class Carousel {
  public readonly featuredProjects = input<ProjectCatalogueDto[]>([]);
  public readonly images = input<string[]>([]);
  public readonly imageAltLabel = input('Project image');
  public readonly projectNumVisible = input(2);
  public readonly projectNumScroll = input(1);
  public readonly projectResponsiveOptions = input([
    {
      breakpoint: '1400px',
      numVisible: 2,
      numScroll: 1
    },
    {
      breakpoint: '1199px',
      numVisible: 3,
      numScroll: 1
    },
    {
      breakpoint: '767px',
      numVisible: 2,
      numScroll: 1
    },
    {
      breakpoint: '575px',
      numVisible: 1,
      numScroll: 1
    }
  ]);

  protected readonly isImageCarousel = computed(() => this.images().length > 0);
  protected readonly showImageControls = computed(() => this.images().length > 1);
  protected readonly showProjectControls = computed(() => this.featuredProjects().length > this.projectNumVisible());
}
