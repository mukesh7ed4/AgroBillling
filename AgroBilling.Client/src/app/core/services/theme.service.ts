import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type Theme = 'dark' | 'light';
export type Lang  = 'en' | 'hi';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly THEME_KEY = 'ab_theme';
  private readonly LANG_KEY  = 'ab_lang';

  private _theme$ = new BehaviorSubject<Theme>(this.getSavedTheme());
  private _lang$  = new BehaviorSubject<Lang>(this.getSavedLang());

  theme$ = this._theme$.asObservable();
  lang$  = this._lang$.asObservable();

  get currentTheme(): Theme { return this._theme$.value; }
  get currentLang():  Lang  { return this._lang$.value;  }

  constructor() { this.applyTheme(this._theme$.value); }

  toggleTheme(): void {
    const next = this._theme$.value === 'dark' ? 'light' : 'dark';
    this._theme$.next(next);
    localStorage.setItem(this.THEME_KEY, next);
    this.applyTheme(next);
  }

  setTheme(theme: Theme): void {
    this._theme$.next(theme);
    localStorage.setItem(this.THEME_KEY, theme);
    this.applyTheme(theme);
  }

  toggleLang(): void {
    const next = this._lang$.value === 'en' ? 'hi' : 'en';
    this._lang$.next(next);
    localStorage.setItem(this.LANG_KEY, next);
  }

  private applyTheme(theme: Theme): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  private getSavedTheme(): Theme {
    return (localStorage.getItem(this.THEME_KEY) as Theme) || 'dark';
  }

  private getSavedLang(): Lang {
    return (localStorage.getItem(this.LANG_KEY) as Lang) || 'en';
  }
}