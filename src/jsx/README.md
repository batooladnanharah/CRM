# JSX

## What is JSX?

**JSX (JavaScript XML)** is a syntax extension for JavaScript that allows you to write HTML-like code inside JavaScript. React uses JSX to describe what the user interface should look like.

Example:

```jsx
function App() {
  return <h1>Hello React!</h1>;
}
```

Although JSX looks like HTML, it is not HTML. It is compiled into JavaScript using tools like Babel.

---

# Why use JSX?

JSX makes React code easier to read and write.

Benefits:

* Easier to understand than using `React.createElement()`
* Combines HTML and JavaScript in one place
* Allows dynamic content using JavaScript expressions
* Improves code readability and maintainability
* Makes UI development faster

Without JSX:

```javascript
const element = React.createElement(
  "h1",
  null,
  "Hello React"
);
```

With JSX:

```jsx
const element = <h1>Hello React</h1>;
```

---

# JSX vs HTML

Although JSX looks like HTML, there are some important differences.

| HTML                            | JSX                                                 |
| ------------------------------- | --------------------------------------------------- |
| `class`                         | `className`                                         |
| `for`                           | `htmlFor`                                           |
| Inline styles use strings       | Inline styles use JavaScript objects                |
| Event names are lowercase       | Event names use camelCase                           |
| Can have multiple root elements | Must return a single parent element (or a Fragment) |

Example:

HTML

```html
<label for="name">Name</label>
```

JSX

```jsx
<label htmlFor="name">Name</label>
```

---

# Rules of JSX

When writing JSX, follow these rules:

### 1. Return a single parent element

Correct:

```jsx
return (
  <div>
    <h1>Hello</h1>
    <p>React</p>
  </div>
);
```

Or use a Fragment:

```jsx
return (
  <>
    <h1>Hello</h1>
    <p>React</p>
  </>
);
```

---

### 2. Close every tag

Correct:

```jsx
<img src="logo.png" />
```

Incorrect:

```jsx
<img src="logo.png">
```

---

### 3. Use `className` instead of `class`

Correct:

```jsx
<div className="container">
```

---

### 4. Use camelCase for attributes and events

Examples:

```jsx
onClick
onChange
tabIndex
htmlFor
```

---

### 5. JavaScript goes inside curly braces `{}`

```jsx
const name = "Batool";

<h1>{name}</h1>
```

---

# Expressions

Anything inside `{}` is treated as a JavaScript expression.

Examples:

```jsx
const age = 26;

<p>{age}</p>

<p>{10 + 5}</p>

<p>{"React".toUpperCase()}</p>
```

You can use:

* Variables
* Functions
* Math operations
* String methods
* Object properties
* Array methods

You **cannot** use statements like:

```javascript
if
for
while
switch
```

Instead, use ternary operators, logical operators, or perform the logic before the `return`.

---

# Fragments

A Fragment lets you group multiple elements without adding an extra HTML element to the page.

Instead of:

```jsx
<div>
  <h1>Hello</h1>
  <p>React</p>
</div>
```

Use:

```jsx
<>
  <h1>Hello</h1>
  <p>React</p>
</>
```

Benefits:

* Cleaner HTML
* No unnecessary wrapper elements
* Better layout control

---

# Lists

Lists are created using JavaScript's `map()` function.

Example:

```jsx
const users = ["Batool", "Ali", "John"];

<ul>
  {users.map((user) => (
    <li key={user}>{user}</li>
  ))}
</ul>
```

React creates one `<li>` element for each item in the array.

---

# Keys

A **key** is a unique identifier used by React when rendering lists.

Example:

```jsx
users.map((user) => (
  <li key={user.id}>{user.name}</li>
));
```

Why are keys important?

* Help React identify which items changed.
* Improve rendering performance.
* Prevent unnecessary re-renders.
* Preserve component state correctly.

**Best practice:** Use a unique ID from your data.

Avoid using the array index as the key unless the list is static and never changes.

---

# Events

Events allow your application to respond to user interactions.

Common React events:

* `onClick`
* `onChange`
* `onSubmit`
* `onBlur`
* `onFocus`
* `onKeyDown`
* `onMouseEnter`
* `onMouseLeave`

Example:

```jsx
function Button() {
  function handleClick() {
    alert("Button clicked!");
  }

  return (
    <button onClick={handleClick}>
      Click Me
    </button>
  );
}
```

Unlike HTML, React events use **camelCase** and receive JavaScript functions instead of strings.

HTML:

```html
<button onclick="showMessage()">
```

React:

```jsx
<button onClick={showMessage}>
```

---

# Summary


* What JSX is and why React uses it.
* The differences between JSX and HTML.
* The basic rules for writing valid JSX.
* How to embed JavaScript expressions in JSX.
* How Fragments avoid unnecessary wrapper elements.
* How to render lists using `map()`.
* Why every list item should have a unique `key`.
* How to handle user interactions using React events.
