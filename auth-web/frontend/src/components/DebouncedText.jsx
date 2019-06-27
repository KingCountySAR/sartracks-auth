import React, { Component } from 'react';
import { throttle, debounce } from "throttle-debounce";

class DebouncedText extends Component {
  constructor(props) {
    super(props);
    this.state = { q: ""};
    this.autocompleteSearchDebounced = debounce(500, this.autocompleteSearch);
    this.autocompleteSearchThrottled = throttle(500, this.autocompleteSearch);
  }

  changeQuery = event => {
    this.setState({ q: event.target.value }, () => {
      const q = this.state.q;
      if (q.length < 5) {
        this.autocompleteSearchThrottled(this.state.q);
      } else {
        this.autocompleteSearchDebounced(this.state.q);
      }
    });
  }

  autocompleteSearch = q => {
    this.props.onChange(q)
  }

  render() {
    const { value, onChange, ...rest } = this.props
    return (
      <input type="text" value={this.state.q} onChange={this.changeQuery} {...rest} />
    )
  }
}

export default DebouncedText