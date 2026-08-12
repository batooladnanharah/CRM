// Lesson 6: Passing an array of strings as a prop
// Arrays can be mapped into a list of JSX elements.

interface SkillsListProps {
  skills: string[];
}

function SkillsList({ skills }: SkillsListProps) {
  return (
    <ul className="skills-list">
      {skills.map((skill) => (
        <li key={skill}>{skill}</li>
      ))}
    </ul>
  );
}

export default SkillsList;
