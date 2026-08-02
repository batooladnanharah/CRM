import  "./jsx/JSX.css";
import Attributes from "./jsx/Attributes";
import BasicJSX from "./jsx/BasicJSX";
import ConditionalRendering from "./jsx/ConditionalRendering";
import Events from "./jsx/Events";
import Expressions from "./jsx/Expressions";
import Fragments from "./jsx/Fragments";
import Lists from "./jsx/Lists";



function App() {
  return (
    <div className="container">
      <h1>React JSX Learning</h1>

      <BasicJSX />

      <Expressions />

      <Attributes />

      <ConditionalRendering />

      <Lists />

      <Fragments />

      <Events />
    </div>
  );
}

export default App;