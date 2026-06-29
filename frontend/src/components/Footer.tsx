import styles from './Footer.module.css';

/**
 * Site footer — portfolio attribution. Shown on every page. Links to the source
 * repo and LinkedIn (no raw email, to avoid spam scraping). The tech line is
 * ordered by emphasis: backend/cloud first, then frontend.
 */
export function Footer() {
  const year = new Date().getFullYear();

  return (
    <footer className={styles.footer}>
      <p className={styles.line}>
        Built by <strong>Mark Vanzuela</strong>
        <span className={styles.sep}>·</span>
        <a href="https://github.com/mark-vanzuela/notes-app" target="_blank" rel="noreferrer">
          Source
        </a>
        <span className={styles.sep}>·</span>
        <a href="https://www.linkedin.com/in/markvanzuela/" target="_blank" rel="noreferrer">
          LinkedIn
        </a>
      </p>
      <p className={styles.tech}>ASP.NET Core · Azure · Terraform · React · © {year}</p>
    </footer>
  );
}
