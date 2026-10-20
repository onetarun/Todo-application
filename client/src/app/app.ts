import {
  afterNextRender,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  Injector,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, skip } from 'rxjs';
import { AuthService } from './core/auth.service';

@Component({
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
  ],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  protected readonly auth = inject(AuthService);

  constructor() {
    // After in-app navigation, move focus to the new page's heading so screen readers announce it
    // (the first page load keeps the browser's default focus).
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        skip(1),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(() => afterNextRender(() => this.focusPageHeading(), { injector: this.injector }));
  }

  protected signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigate(['/login']));
  }

  private focusPageHeading(): void {
    const main = this.host.nativeElement.querySelector<HTMLElement>('main');
    const heading = main?.querySelector<HTMLElement>('h1');
    const target = heading ?? main;
    if (!target) {
      return;
    }
    if (heading && !heading.hasAttribute('tabindex')) {
      heading.setAttribute('tabindex', '-1');
    }
    target.focus();
  }
}
