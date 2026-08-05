// Phase 4: State (useState)
// Each lesson below demonstrates one way of using useState to make
// components interactive and remember data between renders.

import Counter from "./Counter";
import Toggle from "./Toggle";
import LikeButton from "./LikeButton";
import ThemeSwitcher from "./ThemeSwitcher";
import ControlledInput from "./ControlledInput";
import UserForm from "./UserForm";
import ShoppingCart from "./ShoppingCart";
import TodoList from "./TodoList";
import ObjectState from "./ObjectState";
import ArrayState from "./ArrayState";
import BooleanState from "./BooleanState";
import MultipleState from "./MultipleState";
import FunctionalUpdate from "./FunctionalUpdate";
import "./state.css";

function StateApp() {
  return (
    <div className="state-app">
      <h1>Phase 4: State (useState)</h1>

      <div className="lesson">
        <h2>1. Counter</h2>
        <p className="explanation">Increase, decrease, and reset a number in state.</p>
        <Counter />
      </div>

      <div className="lesson">
        <h2>2. Toggle Example</h2>
        <p className="explanation">A boolean state variable shows or hides text.</p>
        <Toggle />
      </div>

      <div className="lesson">
        <h2>3. Like Button</h2>
        <p className="explanation">Two state variables working together: liked + count.</p>
        <LikeButton />
      </div>

      <div className="lesson">
        <h2>4. Theme Switcher</h2>
        <p className="explanation">State decides which CSS class (and look) to apply.</p>
        <ThemeSwitcher />
      </div>

      <div className="lesson">
        <h2>5. Controlled Input</h2>
        <p className="explanation">The input's value comes from state and updates live.</p>
        <ControlledInput />
      </div>

      <div className="lesson">
        <h2>6. User Form</h2>
        <p className="explanation">Two controlled inputs, each with its own state.</p>
        <UserForm />
      </div>

      <div className="lesson">
        <h2>7. Shopping Cart</h2>
        <p className="explanation">Increase/decrease a quantity, with a minimum of 1.</p>
        <ShoppingCart />
      </div>

      <div className="lesson">
        <h2>8. Todo List</h2>
        <p className="explanation">Add and remove items from an array in state.</p>
        <TodoList />
      </div>

      <div className="lesson">
        <h2>9. Object State</h2>
        <p className="explanation">Update one field of an object without losing the rest.</p>
        <ObjectState />
      </div>

      <div className="lesson">
        <h2>10. Array State</h2>
        <p className="explanation">Add and remove user objects from an array.</p>
        <ArrayState />
      </div>

      <div className="lesson">
        <h2>11. Boolean State</h2>
        <p className="explanation">A checkbox enables or disables a button.</p>
        <BooleanState />
      </div>

      <div className="lesson">
        <h2>12. Multiple State Variables</h2>
        <p className="explanation">One component can manage several independent pieces of state.</p>
        <MultipleState />
      </div>

      <div className="lesson">
        <h2>13. Functional Updates</h2>
        <p className="explanation">
          setCount(prev =&gt; prev + 1) is preferred over setCount(count + 1)
          because it always uses the latest state, even across multiple updates
          in the same event.
        </p>
        <FunctionalUpdate />
      </div>
    </div>
  );
}

export default StateApp;
