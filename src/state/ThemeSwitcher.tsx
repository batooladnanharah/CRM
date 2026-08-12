// Lesson 4: Theme Switcher
// State can drive which CSS class is applied, switching the whole look of a section.

import { useState } from "react";

function ThemeSwitcher() {
  const [theme, setTheme] = useState<"light" | "dark">("light");

  return (
    <div>
      <button
        className="state-btn"
        onClick={() => setTheme(theme === "light" ? "dark" : "light")}
      >
        Switch to {theme === "light" ? "Dark" : "Light"} Mode
      </button>
      <div className={`theme-box ${theme === "light" ? "theme-light" : "theme-dark"}`}>
        Current theme: {theme}
      </div>
    </div>
  );
}

export default ThemeSwitcher;
