import { Component, Inject } from '@angular/core';
import { ProjectService } from '../../../services/project.service';

@Inject(ProjectService)
@Component({
  selector: 'app-home',
  templateUrl: './home.html',
  styleUrl: './home.scss',
  imports: [
  ],
})
export class Home {
  private projectService: ProjectService;

  constructor(projectService: ProjectService) {
    this.projectService = projectService;

    this.projectService.getCatalogue().subscribe({
      next: (data) => {
        console.log('Project catalogue:', data);
      },
      error: (err) => {
        console.error('Error fetching project catalogue:', err);
      }
    });
  }
}
