// Phase 3: Props
// Each lesson below demonstrates one specific way of passing props
// from a parent component down to a child component.

import BasicProps from "./BasicProps";
import UserCard from "./UserCard";
import ProductCard from "./ProductCard";
import ProfileCard from "./ProfileCard";
import SkillsList from "./SkillsList";
import Button from "./Button";
import Card from "./Card";
import FunctionProps from "./FunctionProps";
import DefaultProps from "./DefaultProps";
import "./props.css";

function PropsApp() {
  return (
    <div className="props-app">
      <h1>Phase 3: Props</h1>

      <div className="lesson">
        <BasicProps username="batool" age={22} isOnline={true} />
      </div>

      <div className="lesson">
        <h2>2. UserCard (multiple typed props)</h2>
        <p className="explanation">
          Passing name, age, country and image as separate props.
        </p>
        <UserCard
          name="Sara"
          age={25}
          country="Saudi Arabia"
          image="https://i.pravatar.cc/100?img=5"
        />
      </div>

      <div className="lesson">
        <h2>3. ProductCard</h2>
        <p className="explanation">
          Props can be used to compute derived values, like "in stock" status.
        </p>
        <ProductCard
          productName="Wireless Mouse"
          price={25}
          stock={12}
          category="Electronics"
        />
        <ProductCard
          productName="Mechanical Keyboard"
          price={80}
          stock={0}
          category="Electronics"
        />
      </div>

      <div className="lesson">
        <h2>4. Button (variant + disabled props)</h2>
        <p className="explanation">
          The same Button component looks and behaves differently based on props.
        </p>
        <Button text="Primary" variant="primary" />
        <Button text="Secondary" variant="secondary" />
        <Button text="Danger" variant="danger" disabled />
      </div>

      <div className="lesson">
        <h2>5. ProfileCard (object prop)</h2>
        <p className="explanation">
          Related fields are grouped into a single "profile" object prop.
        </p>
        <ProfileCard
          profile={{
            name: "Ahmed",
            title: "Frontend Developer",
            image: "https://i.pravatar.cc/100?img=12",
          }}
        />
      </div>

      <div className="lesson">
        <h2>6. SkillsList (array prop)</h2>
        <p className="explanation">
          An array of strings is passed in and rendered as a list.
        </p>
        <SkillsList skills={["React", "TypeScript", "CSS", "Git"]} />
      </div>

      <div className="lesson">
        <h2>7. Children Prop</h2>
        <p className="explanation">
          Card doesn't know what content it holds — it just renders "children".
        </p>
        <Card title="Reusable Card">
          <p>This paragraph is passed in as children.</p>
          <SkillsList skills={["Reusable", "Flexible"]} />
        </Card>
      </div>

      <div className="lesson">
        <h2>8. Function Props</h2>
        <p className="explanation">
          The parent passes a click handler down; the child calls it on click.
        </p>
        <FunctionProps onGreet={(name) => alert(`Hello, ${name}!`)} />
      </div>

      <div className="lesson">
        <h2>9. Default Props</h2>
        <p className="explanation">
          "greeting" is optional and falls back to a default value.
        </p>
        <DefaultProps name="Layla" />
        <DefaultProps name="Omar" greeting="Good morning" />
      </div>

      <div className="lesson">
        <h2>10. Rendering Multiple Components Using Props</h2>
        <p className="explanation">
          Looping over data and rendering one component per item, each with its own props.
        </p>
        {[
          { name: "Item A", price: 10 },
          { name: "Item B", price: 20 },
          { name: "Item C", price: 30 },
        ].map((item) => (
          <ProductCard
            key={item.name}
            productName={item.name}
            price={item.price}
            stock={5}
            category="Sample"
          />
        ))}
      </div>
    </div>
  );
}

export default PropsApp;
