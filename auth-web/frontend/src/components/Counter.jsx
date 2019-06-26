import React, { Component } from 'react';
import AuthRoute from './auth/AuthRoute';

const Inner = (props) => {
  return 'you are logged in'
}

export class Counter extends Component {
  static displayName = Counter.name;

  constructor (props) {
    super(props);
    this.state = { currentCount: 0 };
    this.incrementCounter = this.incrementCounter.bind(this);
  }

  incrementCounter () {
    this.setState({
      currentCount: this.state.currentCount + 1
    });
  }

  render () {
    return (
      <div>
        <h1>Counter</h1>

        <p>This is a simple example of a React component.</p>
<AuthRoute component={Inner} denied='no authed'></AuthRoute>
        <p>Current count: <strong>{this.state.currentCount}</strong></p>

        <button className="btn btn-primary" onClick={this.incrementCounter}>Increment</button>
      </div>
    );
  }
}
