type Props = {
  themeName: 'pulse' | 'zephyr'
  onBackToCharacters: () => void
  onThemeChange: (themeName: 'pulse' | 'zephyr') => void
}

export default function SettingsPage(props: Props) {
  return (
    <section className="card">
      <h2>Settings</h2>
      <div className="row">
        <button onClick={props.onBackToCharacters}>Back to characters</button>
      </div>
      <div className="grid">
        <label htmlFor="theme-select">Theme</label>
        <select
          id="theme-select"
          value={props.themeName}
          onChange={(e) => props.onThemeChange(e.target.value as 'pulse' | 'zephyr')}
        >
          <option value="pulse">Pulse (default)</option>
          <option value="zephyr">Zephyr</option>
        </select>
      </div>
    </section>
  )
}
