function Lists() {
  const skills = [
    "React",
    "TypeScript",
    "Node.js",
    "ASP.NET Core",
    "MongoDB",
  ];

  return (
    <section className="lesson">
      <h2>5. Rendering Lists</h2>

      <ul>
        {skills.map((skill) => (
          <li key={skill}>{skill}</li>
        ))}
      </ul>
    </section>
  );
}

export default Lists;